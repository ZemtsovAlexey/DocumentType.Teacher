import React, {Component} from 'react';
import { Col, Grid, Row, Thumbnail, Table, ButtonGroup, Button } from 'react-bootstrap';

export class NetSettings extends Component {

    constructor(props) {
        super(props);

        this.state = {
            layers: [],
            imageSrc: ''
        };
    }

    componentDidMount = async () => {
        await fetch('api/net/settings')
            .then(response => response.json())
            .then(data => this.setState({ layers: data }));
    };

    updateLayerType = (i, event) => {
        let layers = this.state.layers;
        layers[i].type = parseInt(event.target.value);

        if (layers[i].type == 2) {
            layers[i].activation = 0;
        }

        this.setState({ layers: layers });
    };

    updateNeuronsCount = (i, event) => {
        let layers = this.state.layers;
        layers[i].neuronsCount = parseInt(event.target.value);
        this.setState({ layers: layers });
    };

    updateKernelSize = (i, event) => {
        let layers = this.state.layers;
        layers[i].kernelSize = parseInt(event.target.value);
        this.setState({ layers: layers });
    };

    prepareTeachBatchFile = async () => {
        await fetch('api/net/teach/batch', {
            method: 'POST'
        });
    };

    showLayerViews = async (i, event) => {
        await fetch(`api/net/layer/${i}`)
            .then(response => response.blob())
            .then(response => {
                //console.log(response);
                //for (let i = 0; i < response.length; i++) {
                //    let outside = URL.createObjectURL(response[i].blob());
                //    console.log(outside);
                //}
                let outside = URL.createObjectURL(response);
                console.log(outside);
                this.setState({ imageSrc: outside });
                //window.open(outside, '_blank')
            });
    };
    
    applySettings = async () => {
        let a = [];
        
        this.state.layers.map(layer => {
            a.push({'Type': layer.type, 'Activation': layer.activation, 'NeuronsCount': layer.neuronsCount, 'KernelSize': layer.kernelSize});
        });

        let data = new FormData();
        data.append('settings', a);
        
        await fetch('api/net/settings/apply', {
            method: 'POST',
            headers: {
                Accept: 'application/json',
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(a)
        })
    };

    render() {
        const layers = this.state.layers;
        let i = -1;

        return (
            <div>
                <img src={this.state.imageSrc} />
                <Table striped bordered condensed hover>
                    <thead>
                        <tr>
                            <th>Type</th>
                            <th>Activation</th>
                            <th>Neurons</th>
                            <th>Kernel</th>
                        </tr>
                    </thead>
                    <tbody>
                        {layers.map(layer => 
                            <tr key={i++}>
                                <td>
                                    <select defaultValue={layer.type} onChange={this.updateLayerType.bind(this, i)}>
                                        <option value="0">Convolution</option>
                                        <option value="1">MLP</option>
                                        <option value="2">MaxPooling</option>
                                    </select>
                                </td>
                                <td>
                                    <select defaultValue={layer.activation} disabled={layer.type == 2}>
                                        <option value="0">None</option>
                                        <option value="1">BipolarSigmoid</option>
                                        <option value="2">Sigmoid</option>
                                        <option value="3">ELU</option>
                                        <option value="4">LeakyReLu</option>
                                        <option value="5">ReLu</option>
                                        <option value="6">LeCunTanh</option>
                                        <option value="7">AbsoluteReLU</option>
                                    </select>
                                </td>
                                <td>
                                    <input
                                        type="number"
                                        value={layer.neuronsCount || ''}
                                        onChange={this.updateNeuronsCount.bind(this, i)}
                                    />
                                </td>
                                <td>
                                    <input
                                        type="number"
                                        value={layer.kernelSize || ''}
                                        onChange={this.updateKernelSize.bind(this, i)}
                                    />
                                    <button onClick={this.showLayerViews.bind(this, i)}>show views</button>
                                </td>
                            </tr>
                        )}
                    </tbody>
                </Table>
                <Row>
                    <Col sm={12}>
                        <ButtonGroup>
                            <Button onClick={this.applySettings}>Apply</Button>
                            <Button onClick={this.prepareTeachBatchFile}>Prepare batch</Button>
                        </ButtonGroup>
                    </Col>
                </Row>
            </div>
        );
    }
}
